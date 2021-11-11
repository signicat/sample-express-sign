import React, {Fragment, useState, useEffect} from "react";
import "./App.css";

const BACKEND_URL = process.env.REACT_APP_BACKEND_URL;

const SignDisplay = () => (
  <section>
    <h1>Welcome to Signicat Express Signature </h1>
    <div className="sign">
      <img src="./images/sign.png" alt="sign" />
      <form action={BACKEND_URL + '/signature-session'} method="POST">
        <button className="btn" type="submit">
          Sign document
        </button>
      </form>
    </div>
  </section>
);

const Message = ({message}) => (
  <section>
    {message.text ?
      <Fragment>
        <h1>{message.text}</h1>
        <img src="./images/sign.png" alt="sign" />
        <div className="btn-wrapper">
          <a className="btn" href={BACKEND_URL + '/download?jwt=' + message.jwt}>
            Download document
          </a>
        </div>
      </Fragment>
      : <p>The sign in process was aborted or has an error.</p>}
  </section>
);

function App() {
  const [message, setMessage] = useState(null);

  useEffect(() => {
    const query = new URLSearchParams(window.location.search);

    if (query.get("success")) {
      setMessage({text: "Your document is successfully signed!", jwt: query.get("idfy-jwt")});
    }
    if (query.get("canceled")) {
      setMessage({});
    }
    if (query.get("error")) {
      setMessage({});
    }
    if (query.get("download")) {
      setMessage({text: "Document is downloaded successfully"})
    }
  }, []);

  return message ? (
    <Message message={message} />
  ) : (
    <SignDisplay />
  );
}

export default App;
