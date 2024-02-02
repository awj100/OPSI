import axios, { type AxiosPromise } from "axios";
import UserAssignment from "@/lib/models/UserAssignment";
import ProjectWithResources from "@/lib/models/ProjectWithResources";

const authToken = "ZjFAdGVzdC5jb206RnJlZWxhbmNlcg==";
const endpointUri = "http://localhost:7071/api/projects";
const headers = {
    Accept: "application/octet-stream",
    Authorization: `Basic ${authToken}`,
    "Content-Type": "application/octet-stream"
};

export async function download(projectId: string, path: string, fileName: string) {
    try {
        const response = await axios({
            headers: headers,
            method: "GET",
            responseType: "blob",
            url: `${endpointUri}/${projectId}/resource/${path}`
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