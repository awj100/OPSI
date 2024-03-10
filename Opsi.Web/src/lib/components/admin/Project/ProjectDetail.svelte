<script lang="ts">
  import { createEventDispatcher } from "svelte";
	import ProjectDetailModel from "../../../models/ProjectDetail";
  import ResourceModel from "../../../models/Resource";
  import TreeNode from "../treeView/TreeNode";
  import TreeView from "../treeView/TreeView.svelte";
  import type CheckChanged from "../../../eventArgs/CheckChanged";

  const dispatch = createEventDispatcher();
	const expandedIds: Array<string> = [];
  let selectedResources: Array<ResourceModel> = [];
  let treeNodes: Array<TreeNode> = [];

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
            text: currentValue
        };
    
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

  function onSelectedResourcesChanged(e: CustomEvent<CheckChanged>) {

    function hasChanged(existingFullNames: string[], newFullNames: string[]) {
      if (existingFullNames.length !== newFullNames.length) {
        return true;
      }

      existingFullNames.sort();
      newFullNames.sort();

      for (let i = 0; i < existingFullNames.length; i++) {
        if (existingFullNames[i] !== newFullNames[i]) {
          return true;
        }
      }

      return false;
    }

    const existingFullNames = selectedResources.flatMap((resource: ResourceModel) => resource.fullName!);
    if (e.detail.isChecked) {
      const newResources = e.detail.resources.filter((resource: ResourceModel) => existingFullNames.indexOf(resource.fullName!) === -1);
      selectedResources = [...selectedResources, ...newResources];
    } else {
      const fullNamesToRemove = e.detail.resources.flatMap((resource: ResourceModel) => resource.fullName!);
      selectedResources = selectedResources.filter((resource: ResourceModel) => fullNamesToRemove.indexOf(resource.fullName!) === -1);
    }
    const newFullNames = selectedResources.flatMap((resource: ResourceModel) => resource.fullName!);

    if (hasChanged(existingFullNames, newFullNames)) {
      dispatch("selectedResourcesChanged", selectedResources);
    }
  }

  export let shouldShowVersions: boolean = false;
  export let projectDetail: ProjectDetailModel;

  treeNodes = getStructure(projectDetail.resources);
</script>

<TreeView
  {treeNodes}
  {shouldShowVersions}
  on:check={onSelectedResourcesChanged} />
