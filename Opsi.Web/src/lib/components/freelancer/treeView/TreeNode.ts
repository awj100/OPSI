import type Resource from "../../../models/Resource";

export default class TreeNode {
    children: Array<TreeNode> = [];
    data: Resource[];
    id: string = "";
    isFile: () => boolean;
    isSelected: boolean = false;
    text: string = "";

    constructor() {
        this.data = [];
        this.isFile = () => this.children.length === 0;
    }
}