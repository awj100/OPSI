<script lang="ts">
  import { fade } from "svelte/transition";
  import Project from "./project/Project.svelte";
  import ProjectStateSelector from "./ProjectStateSelector.svelte";
  import { ProjectStates } from "../../enums/ProjectStates";
  import ProjectSummaryModel from "@/lib/models/ProjectSummary";
  import { Accordion, Button, Grid, Row, Column } from "carbon-components-svelte";
  import { Add } from "carbon-icons-svelte";
  import { getAllByStatus } from "../../services/adminProjectsService";
  import { fetchCount } from "@/lib/stores/projectsStore";

  const defaultProjectState: ProjectStates = ProjectStates.InProgress;

  let continuationToken: string | undefined = undefined;
  let hasContent: boolean;
  let previousProjectState: ProjectStates | undefined = undefined;
  let projectState: ProjectStates = defaultProjectState;
  let projectSummaryModels: ProjectSummaryModel[] = [];

  async function loadMore() {
    const response = await getAllByStatus(projectState, $fetchCount, continuationToken);
    continuationToken = response.data.continuationToken;
    hasContent = true;
    projectSummaryModels = [...projectSummaryModels, ...response.data.items]
  }

  function onProjectStateSelected(e: CustomEvent<ProjectStates>) {
    projectState = e.detail;
  }

  $: if (projectState !== previousProjectState) {
    continuationToken = undefined;
    hasContent = false;
    projectSummaryModels = [];
    loadMore();
  }
</script>

<div in:fade="{{duration: 500}}">
  <ProjectStateSelector projectState={projectState} on:selected={onProjectStateSelected} />

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
