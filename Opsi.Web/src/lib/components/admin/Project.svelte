<script lang="ts">
  import ProjectDetail from "./ProjectDetail.svelte";
  import { AccordionItem, Column, Grid, InlineLoading, Row, Tooltip } from "carbon-components-svelte";
  import ProjectDetailModel from "../../Models/ProjectDetail";
  import ProjectSummaryModel from "../../Models/ProjectSummary";
  import ResourceModel from "../../Models/Resource";
  import { get } from "../../services/projectsService";
  import { UserFollow, Version } from "carbon-icons-svelte";
  import UserAssignment from "./userAssignment/UserAssignment.svelte";

  export let projectSummary: ProjectSummaryModel;

  let projectId = projectSummary.id!;
  let shouldShowUserAssignment: boolean = false;
  let shouldShowVersions: boolean = false;
  let isSelected: boolean = false;
  let projectDetailModel: ProjectDetailModel | undefined = undefined;
  let selectedResources: ResourceModel[] = [];
  let shouldShowVersionVisibilityTooltip: boolean = false;
  let shouldShowAssignmentVisibilityTooltip: boolean = false;

  function toggleVersionVisibility() {
    shouldShowVersions = !shouldShowVersions;
  }

  function toggleAssignmentVisibilityTooltip(shouldShow: boolean) {
    shouldShowAssignmentVisibilityTooltip = shouldShow;
  }

  function toggleVersionVisibilityTooltip(shouldShow: boolean) {
    shouldShowVersionVisibilityTooltip = shouldShow;
  }

  function selectedResourcesChanged(e: CustomEvent<ResourceModel[]>) {
    selectedResources = e.detail;
  }

  async function selectionChanged() {
    isSelected = !isSelected;

    if (!isSelected || projectDetailModel !== undefined) {
      return;
    }

    const { data, status } = await get(projectSummary.id!);
    projectDetailModel = data;
  }
</script>

<AccordionItem title={projectSummary.name} on:click={() => selectionChanged()}>
  {#if !isSelected || projectDetailModel === undefined}
    <InlineLoading description={`Loading ${projectSummary.name}`} />
  {:else}
    <Grid fullWidth noGutterLeft>
      <Row noGutterLeft>
        <Column noGutterLeft sm={4} md={8} lg={8}>
        <ProjectDetail
          {shouldShowVersions}
          projectDetail={projectDetailModel}
          on:selectedResourcesChanged={selectedResourcesChanged} />
        </Column>
        <Column noGutterLeft sm={4} md={1} lg={1}>
          <span class="cursor-pointer"
                role="button"
                tabindex="0"
                on:blur={() => toggleVersionVisibilityTooltip(false)}
                on:click={toggleVersionVisibility}
                on:keypress={toggleVersionVisibility}
                on:focus={() => toggleVersionVisibilityTooltip(true)}
                on:mouseout={() => toggleVersionVisibilityTooltip(false)}
                on:mouseover={() => toggleVersionVisibilityTooltip(true)}>
            <Tooltip
                direction="right"
                icon={Version}
                open={shouldShowVersionVisibilityTooltip}>
              <p>Show or hide the version history for all resources.</p>
            </Tooltip>
          </span>
          <span class="cursor-pointer"
                role="button"
                tabindex="0"
                on:blur={() => toggleAssignmentVisibilityTooltip(false)}
                on:click={() => { shouldShowUserAssignment = selectedResources.length > 0; }}
                on:keypress={() => { shouldShowUserAssignment = selectedResources.length > 0; }}
                on:focus={() => toggleAssignmentVisibilityTooltip(true)}
                on:mouseout={() => toggleAssignmentVisibilityTooltip(false)}
                on:mouseover={() => toggleAssignmentVisibilityTooltip(true)}>
            <Tooltip
                direction="right"
                icon={UserFollow}
                open={shouldShowAssignmentVisibilityTooltip}>
              <p>Assign user for the selected resources.</p>
            </Tooltip>
          </span>
        </Column> 
      </Row>
    </Grid>
  {/if}
</AccordionItem>

<UserAssignment
  openModal={shouldShowUserAssignment}
  projectId={projectId}
  {selectedResources}
  on:close={() => { shouldShowUserAssignment = false; }} />