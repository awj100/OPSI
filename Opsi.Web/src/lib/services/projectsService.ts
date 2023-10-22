import axios, { type AxiosPromise } from "axios";
import PageableResponse from "@/lib/Models/PageableResponse";
import Project from "@/lib/Models/Project";

const authToken = "dXNlckB0ZXN0LmNvbTpBZG1pbmlzdHJhdG9y";
const endpointUri = "http://localhost:7071/api/_admin/projects";
const headers = {
    Accept: "*/*",
    Authorization: `Basic ${authToken}`,
    "Content-Type": "application/json"
};

export async function getAllByStatus(projectStatus: string, pageSize: number): AxiosPromise<PageableResponse<Project>> {
    let endpoint: string = `${endpointUri}/${projectStatus}?pageSize=${pageSize}`;
    return await axios.get<PageableResponse<Project>>(endpoint, {
        headers,
        method: "GET"
    });
};