<script lang="ts">
  import { onMount } from "svelte";
  import ProjectDetailModel from "../../../models/ProjectDetail";
  import ResourceModel from "../../../models/Resource";
  import TreeNode from "../treeView/TreeNode";
  import TreeView from "../treeView/TreeView.svelte";
  import ResourceUpload from "../resourceUpload/ResourceUpload.svelte";

	const expandedIds: Array<string> = [];
  let showUploadModal: boolean = false;
  let treeNodes: Array<TreeNode> = [];

  export let projectDetail: ProjectDetailModel;

  onMount(() => {
    treeNodes = getStructure(projectDetail.resources);
  });

  function getStructure(resources: ResourceModel[]): TreeNode[] {
    const result: Array<TreeNode> = [];
    const level = { result };
    const paths = resources.map((resource: ResourceModel) => resource.fullName!);
    const pathSeparator = "/";

    function reducer(accumulator: any, currentValue: any, idx: number, arr: Array<any>) {
        if (accumulator[currentValue]) {
            return accumulator[currentValue];
        }
    
        accumulator[currentValue] = {
            result: []
        };
    
        const node = {
            children: [],
            id: currentValue,
            isFile: () => false,
            text: currentValue
        };
        node.isFile = () => node.children.length === 0;
    
        if (idx < arr.length - 1) { // If this is a branch, not the leaf...
          node.children = accumulator[currentValue].result;
        }

        accumulator.result.push(node);    
        return accumulator[currentValue];
    }

    function setIdAndData(node: TreeNode, parentPath: string | undefined = undefined) {
      node.id = `${(parentPath ? `${parentPath}${pathSeparator}` : "")}${node.id}`;
      node.data = resources.filter((resource: ResourceModel) => resource.fullName?.startsWith(node.id));

      for (let i = 0; i < (node.children ?? []).length; i++) {
        setIdAndData(node.children[i], node.id);
      }
    }

    paths.forEach(path => {
        path.split(pathSeparator).reduce(reducer, level)
    });

    result.forEach((result: TreeNode) => {
      setIdAndData(result);
    });

    result[0].isSelected = true;
    expandedIds.push(result[0].id.toString());
    return result;
  }

  function onResourceUploaded(e: CustomEvent<string>) {
    // ...
  }

  function onWantsUpload(e: CustomEvent<TreeNode>) {
    console.log("ProjectDetail.onWantsUpload");
    console.log(e.detail);
  }
</script>

<div on:click={() => {showUploadModal = true}}>Click me</div>

<TreeView
  {treeNodes}
  on:wantsUpload={onWantsUpload} />

<ResourceUpload
  openModal={showUploadModal}
  projectDetail={projectDetail}
  on:resourceUploaded={onResourceUploaded} />
