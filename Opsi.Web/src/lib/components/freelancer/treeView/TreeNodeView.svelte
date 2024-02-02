<script lang="ts">
	import { Link } from "carbon-components-svelte";
  import { download } from "../../../services/resourcesService";
  import TreeNode from "./TreeNode";
  import TreeView from "./TreeView.svelte";

	let hasFocus: boolean = false;

  function downloadResource() {
    download(treeNode.data[0].projectId!, treeNode.data[0].fullName!, treeNode.text);
  }

  export let treeNode: TreeNode;

  $: isLeaf = treeNode.children.length === 0;
</script>

<li
  aria-disabled="false"
  aria-expanded={treeNode.isSelected}
  aria-selected={hasFocus}
  role="treeitem">
  {#if treeNode.children.length}
    <div
      aria-disabled="false"
      aria-expanded={treeNode.isSelected}
      aria-selected={hasFocus}
      role="treeitem"
      tabindex="-1"
      on:click|stopPropagation={() => {
        treeNode.isSelected = !treeNode.isSelected;
      }}
      on:keydown="{(e) => {
        console.log(e);
      }}"
      on:blur="{() => {
        hasFocus = false;
      }}"
      on:focus="{() => {
        hasFocus = true;
      }}"
      ><span><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32" fill="currentColor" preserveAspectRatio="xMidYMid meet" width="16" height="16" aria-hidden="true"><path d="M24 12L16 22 8 12z"></path></svg></span>
    </div>
  {:else}
    <div>&nbsp;</div>
  {/if}
  {#if isLeaf}
    <span aria-label={`Download ${treeNode.text}`}
          on:click={() => downloadResource()}
          on:keydown={() => downloadResource()}
          role="link"
          tabindex="-1"
      >
      {treeNode.text}
    </span>
    <!-- <Link href="http://localhost:7071/api/projects/db5653ab-eece-4b15-acb9-e70b0859f1fc/resource/12345/de-CH/test-1.sdlxliff">
      {treeNode.text}
    </Link> -->
  {:else}
    {treeNode.text}
  {/if}
  {#if treeNode.isSelected && treeNode.children.length}
    <TreeView treeNodes={treeNode.children} />
  {/if}
</li>

<style lang="scss">
  div {
    display: inline-block;
    position: relative;
    top: 0.2rem;
    width: 1rem;
  }

  li {
    background-color: transparent;
    padding: 0.5rem 0 0 0.5rem;

    &:not([aria-expanded]) svg,
    &[aria-expanded="false"] svg {
      transform: rotate(-90deg);
      transition: all .3s cubic-bezier(.2,0,.38,.9);
    }
  }

  span {
    cursor: pointer;
  }
</style>