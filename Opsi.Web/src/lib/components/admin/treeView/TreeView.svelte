<script lang="ts">
  import { createEventDispatcher } from "svelte";
  import TreeNode from "./TreeNode";
  import TreeNodeView from "./TreeNodeView.svelte";
  import type CheckChanged from "../../../eventArgs/CheckChanged";

  const dispatch = createEventDispatcher();

  function onCheckChanged(e: CustomEvent<CheckChanged>) {
    dispatch("check", e.detail);
  }

  export let shouldShowVersions: boolean = false;
  export let isChecked: boolean = false;
  export let treeNodes: Array<TreeNode>;
</script>

<ul role="group">
    {#each treeNodes as treeNode ( treeNode.id )}
        <TreeNodeView
          {shouldShowVersions}
          {isChecked}
          {treeNode}
          on:check={(e) => onCheckChanged(e)} />
    {/each}
</ul>

<style lang="scss">
    ul {
        list-style: none;
        margin-left: 1.5rem;
    }
</style>