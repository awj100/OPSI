<script lang="ts">
  import TreeNode from "./TreeNode";
  import TreeNodeView from "./TreeNodeView.svelte";

  export let treeNodes: Array<TreeNode>;
</script>

<ul role="group">
    {#each treeNodes as treeNode ( treeNode.id )}
        <TreeNodeView {treeNode} />
    {/each}
</ul>