import type ResourceVersion from "./ResourceVersion";

export default class Resource {
  assignedBy?: string;
  assignedOnUtc?: Date;
  assignedTo?: string;
  fullName?: string;
  projectId?: string;
  resourceVersions: ResourceVersion[]
  username?: string;
  versionId?: string;
  versionIndex?: number;

  constructor() {
    this.assignedBy = undefined;
    this.assignedOnUtc = undefined;
    this.assignedTo = undefined;
    this.fullName = undefined;
    this.projectId = undefined;
    this.resourceVersions = [];
    this.username = undefined;
    this.versionId = undefined;
    this.versionIndex = undefined;
  }
}