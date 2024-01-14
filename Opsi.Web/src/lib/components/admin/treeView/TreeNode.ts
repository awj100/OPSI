import type Resource from "../../../models/Resource";

export default class TreeNode {
    children: Array<TreeNode> = [];
    data: Resource[];
    id: string = "";
    isSelected: boolean = false;
    text: string = "";

    constructor() {
        this.data = [];
    }
}