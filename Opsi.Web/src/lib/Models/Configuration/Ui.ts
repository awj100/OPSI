import Projects from "./projects";

export default class Ui {
    projects: Projects;

    constructor() {
        this.projects = new Projects();
    }
}