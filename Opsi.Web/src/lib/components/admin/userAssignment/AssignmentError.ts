export default class AssignmentError {
  errorMessage: string;
  resourceFullName: string;

  constructor(resourceFullName: string, errorMessage: string) {
    this.errorMessage = errorMessage;
    this.resourceFullName = resourceFullName;      
  }
}