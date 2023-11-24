import axios, { type AxiosPromise } from "axios";
import PageableResponse from "@/lib/Models/PageableResponse";
import ProjectDetail from "@/lib/Models/ProjectDetail";
import Resource from "@/lib/Models/Resource";
import { ProjectStates } from "../enums/ProjectStates";
import ProjectSummary from "@/lib/Models/ProjectSummary";

const authToken = "dXNlckB0ZXN0LmNvbTpBZG1pbmlzdHJhdG9y";
const endpointUri = "http://localhost:7071/api/_admin/users";
const headers = {
    Accept: "*/*",
    Authorization: `Basic ${authToken}`,
    "Content-Type": "application/json"
};

export async function assignUser(assigneeUsername: string, projectId: string, resource: Resource) {
    const uri: string = `${endpointUri}/${assigneeUsername}/projects/${projectId}/resource/${resource.fullName}`;
    return await axios.put(uri, {        
    }, {
        headers
    });
}