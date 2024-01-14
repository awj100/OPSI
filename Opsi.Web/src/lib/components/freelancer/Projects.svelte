<script lang="ts">
  import { onMount } from "svelte";
  import { fade } from "svelte/transition";
  import Project from "./project/Project.svelte";
  import ProjectWithResourcesModel from "@/lib/models/ProjectWithResources";
  import { Accordion, Button, Grid, Row, Column } from "carbon-components-svelte";
  import { Add } from "carbon-icons-svelte";
  import { getAll } from "../../services/freelancerProjectsService";
  import { fetchCount } from "@/lib/stores/projectsStore";

  let continuationToken: string | undefined = undefined;
  let hasContent: boolean;
  let ProjectWithResourcesModels: ProjectWithResourcesModel[] = [];

  async function loadMore() {
    const response = await getAll($fetchCount);
    hasContent = true;
    ProjectWithResourcesModels = [...ProjectWithResourcesModels, ...response.data]
  }

  onMount(() => {
    loadMore();
  });
</script>

<div in:fade="{{duration: 500}}">
  <Grid noGutterLeft padding>
    <Row>
      <Column sm={4} md={8} lg={12}>
        {#if hasContent}
          <Accordion align="start">
            {#each ProjectWithResourcesModels as projectWithResourcesModel ( projectWithResourcesModel.id )}
              <Project projectWithResources={projectWithResourcesModel} />
            {/each}
          </Accordion>
        {:else}
          <Accordion skeleton count={10} open={false} />
        {/if}
      </Column>
    </Row>
    <Row>
      <Column>
        {#if hasContent}
          <Button
            disabled={!continuationToken}
            icon={Add}
            size="small"
            on:click={loadMore}>More projects</Button>
          {:else}
            <Button skeleton size="small" />
          {/if}
      </Column>
    </Row>
  </Grid>
</div>
