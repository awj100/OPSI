<script lang="ts">
  import { onMount } from "svelte";
  import axios from "../../../../node_modules/axios"
  import PageableResponse from "@/lib/Models/PageableResponse";
  import Project from "@/lib/Models/Project";

  const endpoint = "http://localhost:7071/api/_admin/projects/InProgress?pageSize=10";
  
  let continuationToken: string | undefined = undefined;
  let projects: Array<Project> = [];

  onMount(async function () {
    const response = await axios.get<PageableResponse<Project>>(endpoint, {
      headers: {
        Accept: "*/*",
        Authorization: "Basic dXNlckB0ZXN0LmNvbTpBZG1pbmlzdHJhdG9y",
        "Content-Type": "application/json"
      },
      method: "GET"
    });
    continuationToken = response.data.continuationToken;
    projects = response.data.items;
  });
</script>

{#each projects as project}
  <div>
    <p>{project.name}</p>
  </div>
{/each}