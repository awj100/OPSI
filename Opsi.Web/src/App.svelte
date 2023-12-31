<script lang="ts">
  import type { Readable } from "svelte/store";
  import "../node_modules/carbon-components-svelte/css/all.css";
  import Router from "../node_modules/svelte-spa-router";
  import { Column, Content, Grid, Row, } from "../node_modules/carbon-components-svelte";
  import Header from "./lib/components/common/Header.svelte";
  import HelloFreelancer from "./lib/components/freelancer/text.svelte";
  import AdminProjects from "./lib/components/admin/Projects.svelte";
  import Configuration from "./lib/components/common/Configuration.svelte";
  import NotFound from "./lib/components/common/NotFound.svelte";
  import { setAsReadable } from "./lib/stores/configStore";
  import { loadConfiguration } from "./lib/services/configurationService";

  const routes = {
      "/": AdminProjects,
      "/administrator": AdminProjects,
      "/config": Configuration,
      "/freelancer": HelloFreelancer,
      "*": NotFound,
  };

  const loadedConfig = loadConfiguration();
  const config = setAsReadable(loadedConfig) as any;
  console.log(config);

  const fetchCount = config.ui.projects.fetchCount as Readable<number>;

</script>

<Header />

<Content>
  <Grid fullWidth noGutter>
    <Row>
      <Column>
        <p>fetchCount: {$fetchCount}</p>
      </Column>
    </Row>
    <Row>
        <Column>
          <Router {routes}/>
        </Column>
    </Row>
  </Grid>
</Content>
