<script lang="ts">
  import ProjectAction from "./ProjectAction.svelte";
  import ProjectDetail from "./ProjectDetail.svelte";
  import { AccordionItem, Column, Grid, InlineLoading, Row, Tooltip } from "carbon-components-svelte";
  import ProjectDetailModel from "../../../Models/ProjectDetail";
  import ProjectSummaryModel from "../../../Models/ProjectSummary";
  import ResourceModel from "../../../Models/Resource";
  import { get } from "../../../services/projectsService";
  import { Renew, UserFollow, Version } from "carbon-icons-svelte";
  import UserAssignment from "../userAssignment/UserAssignment.svelte";

  export let projectSummary: ProjectSummaryModel;

  let projectId = projectSummary.id!;
  let shouldShowUserAssignment: boolean = false;
  let shouldShowVersions: boolean = false;
  let isSelected: boolean = false;
  let projectDetailModel: ProjectDetailModel | undefined = undefined;
  let selectedResources: ResourceModel[] = [];

  function onCloseUserAssignment() {
    shouldShowUserAssignment = false;
  }

  function onRefreshClicked() {
    isSelected = false;
    projectDetailModel = undefined;
    selectionChanged();
  }

  function onShowUserAssignmentClicked() {
    shouldShowUserAssignment = selectedResources.length > 0;
  }

  function onUserAssigned() {
    onRefreshClicked();
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

<AccordionItem title={projectSummary.name} on:click={selectionChanged}>
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
        {#if projectDetailModel !== undefined}
          <Column noGutterLeft sm={4} md={1} lg={1} style="padding-top: 1.2rem;">
            <ProjectAction
              icon={Renew}
              tooltip={`Refresh the view of ${projectDetailModel.name}.`}
              on:click={onRefreshClicked} />
            <ProjectAction
              icon={Version}
              tooltip="Show or hide the version history for all resources." />
            <ProjectAction
              icon={UserFollow}
              tooltip="Assign user for the selected resources."
              on:click={onShowUserAssignmentClicked} />
          </Column>
        {/if}
      </Row>
    </Grid>
  {/if}
</AccordionItem>

<UserAssignment
  openModal={shouldShowUserAssignment}
  projectId={projectId}
  {selectedResources}
  on:close={onCloseUserAssignment}
  on:userAssigned={onUserAssigned} />