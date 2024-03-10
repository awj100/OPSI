export default abstract class User {
    authToken: string;
    username: string;

    constructor() {
        this.authToken = "";
        this.username = "";
    }
}