export default class PageableResponse<T> {
    continuationToken?: string;
    items: Array<T>;

    constructor() {
        this.continuationToken = undefined;
        this.items = [];
    }
}