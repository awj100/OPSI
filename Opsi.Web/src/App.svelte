<script lang="ts">
  import "../node_modules/carbon-components-svelte/css/all.css";
  import Router from "../node_modules/svelte-spa-router";
  import { Column, Content, Grid, Row, } from "../node_modules/carbon-components-svelte";
  import Header from "./lib/components/common/Header.svelte";
  import HelloFreelancer from "./lib/components/freelancer/text.svelte";
  import AdminProjects from "./lib/components/admin/Projects.svelte"
  import NotFound from "./lib/components/common/NotFound.svelte"

  const routes = {
      "/": AdminProjects,
      "/administrator": AdminProjects,
      "/freelancer": HelloFreelancer,
      "*": NotFound,
  };
</script>

<Header />

<Content>
  <Grid fullWidth noGutter>
    <Row>
        <Column>
          <Router {routes}/>
        </Column>
    </Row>
  </Grid>
</Content>
