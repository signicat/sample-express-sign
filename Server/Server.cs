using System;
using System.IO;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Signicat.Express;
using Signicat.Express.Signature;

namespace Server
{
    class Server
    {
        public static void Main(string[] args)
        {
            WebHost.CreateDefaultBuilder(args)
                .UseUrls("http://localhost:4242")
                .UseWebRoot(".")
                .UseStartup<Startup>()
                .Build()
                .Run();
        }
    }

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        
        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().AddNewtonsoftJson();

            // Register Signature service with ClientID, ClientSecret and Scopes
            services.AddSingleton<ISignatureService>(c => new SignatureService(
                Configuration["Signicat:ClientId"],
                Configuration["Signicat:ClientSecret"],
                new List<OAuthScope>() { OAuthScope.DocumentRead, OAuthScope.DocumentWrite, OAuthScope.DocumentFile }
            ));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseRouting();
            app.UseStaticFiles();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
    }

    [Route("")]
    [ApiController]
    public class SignController : Controller
    {
        private readonly ISignatureService _signatureService;
        private readonly IWebHostEnvironment _env;
        private readonly string _frontendAppUrl;

        public SignController(
            ISignatureService signatureService,
            IConfiguration configuration,
            IWebHostEnvironment env)
        {
            _signatureService = signatureService;
            _frontendAppUrl = configuration["FrontendAppUrl"];
            _env = env;
        }

        [HttpPost("signature-session")]
        public async Task<ActionResult> Create()
        {
            // Get local file to be signed
            var filePath = Path.Combine(_env.ContentRootPath, "letter_of_intent.pdf");
            var data = await System.IO.File.ReadAllBytesAsync(filePath);

            // Configure the signing settings
            var options = new DocumentCreateOptions()
            {
                Title = "Sign job",

                // Set the redirect and eID-methods
                Signers = new List<SignerOptions>()
                {
                    new SignerOptions()
                    {
                        RedirectSettings = new RedirectSettings()
                        {
                            RedirectMode = RedirectMode.Redirect,
                            Error = _frontendAppUrl + "?error=true",
                            Cancel = _frontendAppUrl + "?canceled=true",
                            Success = _frontendAppUrl + "?success=true"
                        },
                        SignatureType = new SignatureType()
                        {
                            Mechanism = SignatureMechanism.Identification,
                            SignatureMethods = new List<SignatureMethod>()
                            {
                                SignatureMethod.NoBankIdNetcentric,
                                SignatureMethod.Mitid,
                                SignatureMethod.DkNemid
                            }

                        },
                        ExternalSignerId = Guid.NewGuid().ToString(),
                    }
                },
                ContactDetails = new ContactDetails()
                {
                    Email = "your@company.com"
                },

                // Reference for internal use 
                ExternalId = Guid.NewGuid().ToString(),

                // Optional: notifications for signers. See API for details
                Notification = new Notification()
                {
                    SignRequest = new SignRequest()
                    {
                        Email = new List<Email>()
                        {
                            new Email()
                            {
                                Language = Language.NO,
                                Subject = "Subject text",
                                Text = "The text of the email",
                                SenderName = "Senders Name"
                            }
                        }
                    }
                },

                // Optional: Retrieve social security number of signers
                Advanced = new Advanced()
                {
                    GetSocialSecurityNumber = true,
                },

                // The document to be signed and format
                DataToSign = new DataToSign()
                {
                    FileName = "letterOfIntent.pdf",
                    Base64Content = Convert.ToBase64String(data),
                    Packaging = new Packaging()
                    {
                        SignaturePackageFormats = new List<SignaturePackageFormat>
                        {
                            SignaturePackageFormat.Pades
                        }
                    }
                }
            };

            // Create document with the settings specified
            var res = await _signatureService.CreateDocumentAsync(options);

            // Redirect user to the URL retrieved from the SDK
            Response.Headers.Add("Location", res.Signers[0].Url);
            return new StatusCodeResult(303);
        }
        
        [HttpGet("download")]
        public async Task<ActionResult> Download([FromQuery] string jwt)
        {
            // Extract DocumentID from JWT
            var documentId = GetDocumentIdFromJwt(jwt);

            bool ready;
            do
            {
                var status = await _signatureService.GetDocumentStatusAsync(documentId);
                ready = status.CompletedPackages.Contains(FileFormat.Pades);
                
                if (!ready) await Task.Delay(1000);

            } while (!ready);
            
            // Download PAdES when it's ready
            var stream = await _signatureService.GetFileAsync(documentId, FileFormat.Pades);
            
            return File(stream, "application/pdf", "letter_of_intent_PAdES.pdf");
        }

        private Guid GetDocumentIdFromJwt(string jwt)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);
            var documentIdString = token.Claims.First(claim => claim.Type == "DocumentId").Value;
            return new Guid(documentIdString);
        }
    }
}
