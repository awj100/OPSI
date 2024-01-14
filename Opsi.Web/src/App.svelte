<script lang="ts">
  import "../node_modules/carbon-components-svelte/css/all.css";
  import Router from "../node_modules/svelte-spa-router";
  import { Column, Content, Grid, Row, } from "../node_modules/carbon-components-svelte";
  import Header from "./lib/components/common/Header.svelte";
  import AdminProjects from "./lib/components/admin/Projects.svelte";
  import FreelancerProjects from "./lib/components/freelancer/Projects.svelte";
  import Configuration from "./lib/components/common/Configuration.svelte";
  import NotFound from "./lib/components/common/NotFound.svelte";
  import { initConfig } from "@/lib/services/configurationService";

  const routes = {
      "/": AdminProjects,
      "/administrator": AdminProjects,
      "/config": Configuration,
      "/freelancer": FreelancerProjects,
      "*": NotFound,
  };

  initConfig();

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
