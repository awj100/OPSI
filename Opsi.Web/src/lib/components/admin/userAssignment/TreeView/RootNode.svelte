<script lang="ts">
  import type Node from "./Node";
  import TextNode from "./TextNode.svelte";

  export let nodes: Node[] = [];
</script>

<ul>
    {#each nodes as resourceNode (resourceNode.text)}
        <TextNode node={resourceNode} />
    {/each}
</ul>
