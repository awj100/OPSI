<script lang="ts">
  import { InlineLoading } from "carbon-components-svelte";
  import RootNode from "./RootNode.svelte";
  import type Node from "./Node";

  export let node: Node;

  let isFile: boolean = node.children.length === 0;
</script>

<li class:file={isFile}>
    <span>{node.text}</span>&nbsp;
    {#if isFile}
        <div>
            {#if node.status === "active"}
                <InlineLoading status="active" />
            {:else if node.status === "error"}
                <InlineLoading status="error" />
            {:else if node.status === "inactive"}
                <InlineLoading status="inactive" />
            {:else if node.status === "finished"}
                <InlineLoading status="finished" />
            {:else}
                <InlineLoading status="inactive" />
            {/if}
        </div>
    {/if}

    {#if !isFile}
        <RootNode nodes={node.children} />
    {/if}
</li>