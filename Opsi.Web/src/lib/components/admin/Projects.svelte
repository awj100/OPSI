<script lang="ts">
  import Project from "./Project.svelte";
  import { ProjectStates } from "../../enums/ProjectStates";
  import ProjectSummaryModel from "@/lib/Models/ProjectSummary";
  import { Accordion, Button, Grid, Row, Column } from "carbon-components-svelte";
  import { Add } from "carbon-icons-svelte";
  import { getAllByStatus } from "../../services/projectsService";

  const pageSize: number = 10;

  let continuationToken: string | undefined = undefined;
  let hasContent: boolean;
  let previousProjectState: ProjectStates | undefined = undefined;
  let projectSummaryModels: ProjectSummaryModel[] = [];

  async function loadMore() {
    const response = await getAllByStatus(projectState, pageSize, continuationToken);
    continuationToken = response.data.continuationToken;
    hasContent = true;
    projectSummaryModels = [...projectSummaryModels, ...response.data.items]
  }

  export let projectState: ProjectStates;

  $: if (projectState !== previousProjectState) {
    continuationToken = undefined;
    hasContent = false;
    projectSummaryModels = [];
    loadMore();
  }
</script>

<Grid noGutterLeft padding>
  <Row>
    <Column sm={4} md={8} lg={12}>
      {#if hasContent}
        <Accordion align="start">
          {#each projectSummaryModels as projectSummaryModel ( projectSummaryModel.id )}
            <Project projectSummary={projectSummaryModel} />
          {/each}
        </Accordion>
      {:else}
        <Accordion skeleton count={10} open={false} />
      {/if}
    </Column>
  </Row>
  <Row>
    <Column>
      <Button
        disabled={!continuationToken}
        icon={Add}
        size="small"
        on:click={loadMore}>More projects</Button>
    </Column>
  </Row>
</Grid>