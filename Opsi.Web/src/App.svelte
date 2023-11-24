<script lang="ts">
  import "../node_modules/carbon-components-svelte/css/all.css";
  import { Column, Content, Grid, Row } from "../node_modules/carbon-components-svelte";
  import Header from "./lib/components/common/Header.svelte";
  import ModeSelector from "./lib/components/common/ModeSelector.svelte";
  import Projects from "./lib/components/admin/Projects.svelte"
  import ProjectStateSelector from "./lib/components/admin/ProjectStateSelector.svelte"
  import { ProjectStates } from "./lib/enums/ProjectStates";

  const defaultProjectState: ProjectStates = ProjectStates.InProgress;

  function onProjectStateSelected(e: CustomEvent<ProjectStates>) {
    projectState = e.detail;
  }

  let projectState: ProjectStates = defaultProjectState;
</script>

<Content>
  <Grid fullWidth noGutter>
    <Header/>
    <Row>
        <Column sm={4} md={1}>
          <ModeSelector />
        </Column>
        <Column>
          <ProjectStateSelector projectState={projectState} on:selected={onProjectStateSelected} />
          <Projects {projectState} />
        </Column>
    </Row>
  </Grid>
</Content>
