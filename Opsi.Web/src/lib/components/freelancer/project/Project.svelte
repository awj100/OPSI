<script lang="ts">
  import ProjectDetail from "./ProjectDetail.svelte";
  import { AccordionItem, Column, Grid, InlineLoading, Row } from "carbon-components-svelte";
  import ProjectDetailModel from "../../../models/ProjectDetail";
  import ProjectWithResourcesModel from "../../../models/ProjectWithResources";
  import { get } from "../../../services/adminProjectsService";

  export let projectWithResources: ProjectWithResourcesModel;

  let isSelected: boolean = false;
  let projectDetailModel: ProjectDetailModel | undefined = undefined;

  async function selectionChanged() {
    isSelected = !isSelected;

    if (!isSelected || projectDetailModel !== undefined) {
      return;
    }

    const { data } = await get(projectWithResources.id!);
    projectDetailModel = data;
  }
</script>

<AccordionItem title={projectWithResources.name} on:click={selectionChanged} class="pad-t-o">
  {#if !isSelected || projectDetailModel === undefined}
    <InlineLoading description={`Loading ${projectWithResources.name}`} />
  {:else}
    <Grid fullWidth noGutterLeft>
      <Row noGutterLeft>
        <Column noGutterLeft sm={4} md={8} lg={8} class="pad-t-0">
          <ProjectDetail projectDetail={projectDetailModel} />
        </Column>
      </Row>
    </Grid>
  {/if}
</AccordionItem>

