import axios, { type AxiosPromise } from "axios";
import PageableResponse from "@/lib/Models/PageableResponse";
import ProjectDetail from "@/lib/Models/ProjectDetail";
import Resource from "@/lib/Models/Resource";
import { ProjectStates } from "../enums/ProjectStates";
import ProjectSummary from "@/lib/Models/ProjectSummary";

const authToken = "dXNlckB0ZXN0LmNvbTpBZG1pbmlzdHJhdG9y";
const endpointUri = "http://localhost:7071/api/_admin/projects";
const headers = {
    Accept: "*/*",
    Authorization: `Basic ${authToken}`,
    "Content-Type": "application/json"
};

export async function get(projectId: string): AxiosPromise<ProjectDetail> {
    const uri: string = `${endpointUri}/${projectId}`;
    return await axios.get<ProjectDetail>(uri, {
        headers,
        method: "GET"
    });
}

export async function getAllByStatus(projectState: ProjectStates, pageSize: number, continutationToken?: string): AxiosPromise<PageableResponse<ProjectSummary>> {
    const qsContintuationToken = !!continutationToken ? `&continuationToken=${continutationToken}` : "";
    const uri: string = `${endpointUri}/${projectState}?pageSize=${pageSize}${qsContintuationToken}`;
    return await axios.get<PageableResponse<ProjectSummary>>(uri, {
        headers,
        method: "GET"
    });
}