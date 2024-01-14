import UserAssignment from "./UserAssignment";

export default class ProjectSummary {
  id?: string;
  name?: string;
  resources?: UserAssignment[];
  state?: string;
  username?: string;
   
  constructor() {
    this.id = undefined;
    this.name = undefined;
    this.resources = [];
    this.state = undefined;
    this.username = undefined;
  }
}