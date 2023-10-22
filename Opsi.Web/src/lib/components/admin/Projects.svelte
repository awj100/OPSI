<script lang="ts">
  import { onMount } from "svelte";
  import Project from "@/lib/Models/Project";
  import { getAllByStatus } from "../../services/projectsService";

  const pageSize: number = 10;

  let continuationToken: string | undefined = undefined;
  let projects: Array<Project> = [];

  onMount(async function () {
    const response = await getAllByStatus("InProgress", pageSize);
    continuationToken = response.data.continuationToken;
    projects = response.data.items;
  });
</script>

{#each projects as project}
  <div>
    <p>{project.name}</p>
  </div>
{/each}