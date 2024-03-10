<script lang="ts">
  import { createEventDispatcher } from "svelte";
  import { Upload } from "carbon-icons-svelte";
  import TreeNode from "./TreeNode";
  import { download } from "../../../services/resourcesService";
  import { freelancerAuthToken } from "@/lib/stores/usersStore";

  export let treeNodes: Array<TreeNode>;

  const dispatch = createEventDispatcher();
	let hasFocus: boolean = false;

  function downloadResource(treeNode: TreeNode) {
    download($freelancerAuthToken, treeNode.data[0].projectId!, treeNode.data[0].fullName!, treeNode.text);
  }

  function onWantsUpload(treeNode: TreeNode) {
    console.log("TreeView.onWantsUpload");
    dispatch("wantsUpload", treeNode);
  }
</script>

<ul role="group">
  {#each treeNodes as treeNode ( treeNode.id )}
      <li
      aria-disabled="false"
      aria-expanded={treeNode.isSelected}
      aria-selected={hasFocus}
      role="treeitem">
      {#if !treeNode.isFile()}
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
      {#if treeNode.isFile()}
        <span aria-label={`Download ${treeNode.text}`}
              on:click={() => downloadResource(treeNode)}
              on:keydown={() => downloadResource(treeNode)}
              role="link"
              tabindex="-1">
          {treeNode.text}
        </span>
        <span class="upload"
              aria-label={`Upload new version: ${treeNode.text}`}
              role="button"
              on:click={() => onWantsUpload(treeNode)}
              on:keyup={() => onWantsUpload(treeNode)}
              tabindex="-1">
          <Upload title={`Upload new version: ${treeNode.text}`} />
        </span>
        <!-- <Link href="http://localhost:7071/api/projects/db5653ab-eece-4b15-acb9-e70b0859f1fc/resource/12345/de-CH/test-1.sdlxliff">
          {treeNode.text}
        </Link> -->
      {:else}
        {treeNode.text}
      {/if}
      {#if treeNode.isSelected && !treeNode.isFile()}
        <svelte:self treeNodes={treeNode.children} />
      {/if}
    </li>
  {/each}
</ul>

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

    &.upload {
      display: inline-block;
      position: relative;
      top: 0.3rem;
      margin-left: 1rem;
    }
  }
</style>