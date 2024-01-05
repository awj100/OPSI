<script lang="ts">
  import { onMount } from "svelte";
  import { User, UserFilled } from "carbon-icons-svelte";
  
  const unassignedUsernameText: string = "Unassigned";
  let assignedUsernameText: string;
  let userIsAssigned: boolean = false;

  onMount(() => {
    userIsAssigned = !!assignedUsername;
    assignedUsernameText = userIsAssigned ? assignedUsername! : unassignedUsernameText;
  });

  export let assignedUsername: string | undefined = undefined;
</script>

<div class="icon-with-label">
    {#if userIsAssigned}
        <UserFilled aria-labelledby="lblShowVersions" id="imgShowVersions" />
    {:else}
        <User aria-labelledby="lblShowVersions" id="imgShowVersions" />
    {/if}
    <label for="imgShowVersions" id="lblShowVersions">{assignedUsernameText}</label>
</div>