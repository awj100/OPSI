import axios from "axios";

interface IHeaders {
    Accept: string,
    Authorization?: string,
    "Content-Type": string
}

const endpointUri = "http://localhost:7071/api/projects";
const standardHeaders: IHeaders = {
    Accept: "application/octet-stream",
    "Content-Type": "application/octet-stream"
};

export async function download(authToken: string, projectId: string, path: string, fileName: string) {
  const headers = getAuthenticatedHeaders(authToken);

  try {
    const response = await axios({
      headers: headers,
      method: "GET",
      responseType: "blob",
      url: `${endpointUri}/${projectId}/resources/${path}`
    });

    const contentDisposition =
    response.headers["content-disposition"];

    console.log("contentDisposition: ", contentDisposition);

    const href = window.URL.createObjectURL(response.data);

    const anchorElement = document.createElement("a");

    anchorElement.href = href;
    anchorElement.download = fileName;

    document.body.appendChild(anchorElement);
    anchorElement.click();

    document.body.removeChild(anchorElement);
    window.URL.revokeObjectURL(href);
  } catch (error) {
    console.log(error);
  }
}

export function upload(authToken: string, projectId: string, restOfPath: string, file: File) {
  const headers = getAuthenticatedHeaders(authToken);

  return getFileByteArray(file)
    .then(async (byteArray) => {
      try {
        await axios.post(`${endpointUri}/${projectId}/resources/${restOfPath}`, byteArray, { headers: headers });
      } catch (error: any) {
        if (axios.isAxiosError(error) && error.response) {
          throw new Error(error.response.data);
        } else {
          throw error;
        }
      };
    })
    .catch((error: any) => {
      throw error;
    });
}

function getFileByteArray(file: File): Promise<Uint8Array> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();

    // Set up an event listener for when the file is loaded.
    reader.onload = (event: ProgressEvent<FileReader>) => {
      // 'result' contains the contents of the file as an ArrayBuffer.
      const arrayBuffer = event.target?.result as ArrayBuffer;

      // Create a Uint8Array from the ArrayBuffer.
      const byteArray = new Uint8Array(arrayBuffer);

      // Resolve the promise with the byte array.
      resolve(byteArray);
    };

    // Set up an event listener for errors.
    reader.onerror = (_: ProgressEvent<FileReader>) => {
      reject(new Error("Error reading the file."));
    };

    // Read the file as an ArrayBuffer.
    reader.readAsArrayBuffer(file);
  });
}

function getAuthenticatedHeaders(authToken: string): object {
  const thisHeaders = { ...standardHeaders };
  thisHeaders.Authorization = `Basic ${authToken}`;

  return thisHeaders
}
