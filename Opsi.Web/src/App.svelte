<script lang="ts">
  import "../node_modules/carbon-components-svelte/css/all.css";
  import { Route, Router, Link } from "../node_modules/svelte-routing";
  import { Column, Content, Grid, Row, } from "../node_modules/carbon-components-svelte";
  import Header from "./lib/components/common/Header.svelte";
  import HelloFreelancer from "./lib/components/freelancer/text.svelte";
  import Projects from "./lib/components/admin/Projects.svelte"
</script>

<Header />

<Content>
  <Grid fullWidth noGutter>
    <Row>
        <Column>
          <Router>
            <Route path="/administrator" component={Projects} />
          </Router>
          <Router>
            <Route path="/freelancer" component={HelloFreelancer} />
          </Router>
        </Column>
    </Row>
  </Grid>
</Content>
