import Projects from "./Projects";

export default class Ui {
    projects: Projects;

    constructor() {
        this.projects = new Projects();
    }
}