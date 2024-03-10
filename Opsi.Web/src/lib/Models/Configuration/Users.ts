import Administrator from "./Administrator";
import Freelancer from "./Freelancer";

export default class Users {
  administrator: Administrator;
  freelancer: Freelancer;

  constructor() {
    this.administrator = new Administrator();
    this.freelancer = new Freelancer();
  }
}