<script lang="ts">
	import { createEventDispatcher } from "svelte";
  import { Checkbox, Link, Truncate } from "carbon-components-svelte";
  import TreeNode from "./TreeNode";
  import TreeView from "./TreeView.svelte";
  import AssignedUser from "./AssignedUser.svelte";
  import VersionHistory from "./VersionHistory.svelte";

	const dispatch = createEventDispatcher();
  let hasFocus: boolean = false;

  function onCheckChanged(e: CustomEvent<Boolean>) {
    dispatch("check", {
      isChecked: isChecked,
      resources: treeNode.data
    });
  }

  export let shouldShowVersions: boolean = false;
  export let isChecked: boolean;
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
    <Checkbox
      bind:checked={isChecked}
      disabled={!!treeNode.data[0].assignedTo}
      on:check={onCheckChanged} />
    <Link href="~">
      {treeNode.text}
    </Link>
    <AssignedUser assignedUsername={treeNode.data[0].assignedTo} />
    <VersionHistory {shouldShowVersions} versions={treeNode.data[0].resourceVersions} />
  {:else}
    <Checkbox
      bind:checked={isChecked}
      id={treeNode.id}
      labelText={treeNode.text}
      name={treeNode.id}
      on:check={onCheckChanged}
      class="inline-block" />
  {/if}
  {#if treeNode.isSelected && treeNode.children.length}
    <TreeView
      bind:isChecked={isChecked}
      on:check
      treeNodes={treeNode.children} />
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
    padding: 0 0 0 0.5rem;

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