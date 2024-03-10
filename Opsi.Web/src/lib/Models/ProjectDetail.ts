import type Resource from "./Resource";

export default class ProjectDetail {
  id?: string;
  name?: string;
  resources: Resource[];
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