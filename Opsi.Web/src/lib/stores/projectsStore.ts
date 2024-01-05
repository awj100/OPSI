import { writable } from "svelte/store";
import Project from "../Models/ProjectDetail";

export const fetchCount = writable(0);

function createProjectStore() {
    const { subscribe, set } = writable<Project>(undefined);

    return {
        subscribe,
        select: (project: Project) => set(project)
    };
}

export const selectedProject = createProjectStore();
