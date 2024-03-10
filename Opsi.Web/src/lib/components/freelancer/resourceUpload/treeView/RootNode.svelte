<script lang="ts">
  import { Checkbox } from "carbon-components-svelte";
  import type Node from "./Node";

  export let nodes: Node[] = [];
</script>

<ul>
    {#each nodes as node (node.text)}
        <li class:file={node.isFile()}>
          {#if node.isFile()}
            <span class="checkbox"><Checkbox labelText={node.text} checked={node.isSelected && node.children.length === 0} readonly={true} /></span>
          {:else}
            <span>{node.text}</span>
          {/if}
          {#if !node.isFile()}
            <svelte:self nodes={node.children} />
          {/if}
        </li>
    {/each}
</ul>

<style lang="scss">
  span {
    &.checkbox {
      display: inline-block !important;
    }
  }
</style>