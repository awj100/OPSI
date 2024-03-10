export default interface ResourceNode {
    children: ResourceNode[];
    isFile(): boolean;
    isSelected: boolean;
    text: string;
}