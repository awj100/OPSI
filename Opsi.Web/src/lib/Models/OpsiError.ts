export default class OpsiError extends Error {
  displayTitle: string = "Error";
  uniqueId: number = new Date().getTime();

  constructor(displayTitle: string, message: string) {
    super();
    this.displayTitle = displayTitle;
    this.message = message;
  }
}