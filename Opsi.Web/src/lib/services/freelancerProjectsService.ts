import axios, { type AxiosPromise } from "axios";
import UserAssignment from "@/lib/models/UserAssignment";
import ProjectWithResources from "@/lib/models/ProjectWithResources";

const authToken = "ZjFAdGVzdC5jb206RnJlZWxhbmNlcg==";
const endpointUri = "http://localhost:7071/api/projects";
const headers = {
    Accept: "*/*",
    Authorization: `Basic ${authToken}`,
    "Content-Type": "application/json"
};

export async function get(projectId: string): AxiosPromise<UserAssignment> {
    const uri: string = `${endpointUri}/${projectId}`;
    return await axios.get<UserAssignment>(uri, {
        headers,
        method: "GET"
    });
}

export async function getAll(pageSize: number): AxiosPromise<ProjectWithResources[]> {
    const uri: string = `${endpointUri}/?pageSize=${pageSize}`;
    return await axios.get<ProjectWithResources[]>(uri, {
        headers,
        method: "GET"
    });
}