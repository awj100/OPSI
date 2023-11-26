import ResourceModel from "../../../Models/Resource";

export default class UserAssignment {
    username: string;
    resources: ResourceModel[];
  
    constructor(username: string, resources: ResourceModel[]) {
      this.username = username;
      this.resources = [];
    }
  }