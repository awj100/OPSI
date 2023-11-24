export default class ResourceVersion {
  username?: string;
  versionId?: string;
  versionIndex?: number;

  constructor() {
    this.username = undefined;
    this.versionId = undefined;
    this.versionIndex = undefined;
  }
}