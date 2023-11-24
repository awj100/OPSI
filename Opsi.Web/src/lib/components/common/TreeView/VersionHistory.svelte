<script lang="ts">
  import { onMount } from "svelte";
  import { ListItem, UnorderedList } from "carbon-components-svelte";
  import Version from "carbon-icons-svelte/lib/Version.svelte";
  import type ResourceVersion from "../../../Models/ResourceVersion";

  let shouldShowVersions2: boolean = false;
  let showHideVersionsText: string;

  function toggleVisibility() {
    shouldShowVersions2 = !shouldShowVersions2;
    setShowHideText();
  }

  function setShowHideText() {
    showHideVersionsText = `${( shouldShowVersions2 ? "Hide" : "Show")} versions`;
  }

  onMount(() => {
    setShowHideText();
  });

  export let shouldShowVersions: boolean = false;
  export let versions: ResourceVersion[];

  $: console.log(`VersionHistory: ${shouldShowVersions}`);
</script>

<div>
  <div
    class="icon-with-label"
    role="button"
    tabindex="0"
    on:click={toggleVisibility}
    on:keypress={toggleVisibility}>
    <Version
      aria-labelledby="lblShowVersions"
      id="imgShowVersions"
      class="cursor-pointer" />
    <label
      class="cursor-pointer"
      for="imgShowVersions"
      id="lblShowVersions">
        {showHideVersionsText}
    </label>
  </div>
  {#if shouldShowVersions}
    <UnorderedList nested={true}>
      {#each versions as version}
        <ListItem>v{version.versionIndex}: {version.username}</ListItem>
      {/each}
    </UnorderedList>
  {/if}
</div>
