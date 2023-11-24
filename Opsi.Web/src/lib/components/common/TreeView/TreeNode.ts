import type Resource from "../../../Models/Resource";

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