import Users from "./Users";
import Ui from "./Ui";

export default class Configuration {
  ui: Ui;
  users: Users;

  constructor() {
    this.ui = new Ui();
    this.users = new Users();
  }
}