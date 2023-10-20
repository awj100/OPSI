<script lang="ts">
  import AutoAdjust from "@smui/top-app-bar/src/AutoAdjust.svelte";
  import Header from "./lib/components/Header.svelte";
  import Projects from "./lib/components/admin/Projects.svelte"
  import TopAppBar from "@smui/top-app-bar/src/TopAppBar.svelte";

  import Drawer, { AppContent, Content } from '@smui/drawer';
  import List, { Item, Text } from '@smui/list';

  let clicked = 'nothing yet';

  let topAppBar: TopAppBar;
</script>

<Header/>

<!-- <AutoAdjust {topAppBar}>
  <Projects/>
</AutoAdjust> -->

<div class="drawer-container">
  <Drawer>
    <Content>
      <List>
        <Item
          href="javascript:void(0)"
          on:click={() => (clicked = 'Gray Kittens')}
        >
          <Text>Gray Kittens</Text>
        </Item>
        <Item
          href="javascript:void(0)"
          on:click={() => (clicked = 'A Space Rocket')}
        >
          <Text>A Space Rocket</Text>
        </Item>
        <Item
          href="javascript:void(0)"
          on:click={() => (clicked = '100 Pounds of Gravel')}
        >
          <Text>100 Pounds of Gravel</Text>
        </Item>
        <Item
          href="javascript:void(0)"
          on:click={() => (clicked = 'All of the Shrimp')}
        >
          <Text>All of the Shrimp</Text>
        </Item>
        <Item
          href="javascript:void(0)"
          on:click={() => (clicked = 'A Planet with a Mall')}
        >
          <Text>A Planet with a Mall</Text>
        </Item>
      </List>
    </Content>
  </Drawer>

  <AppContent class="app-content">
    <main class="main-content">
      App content.
      <br />
      <pre class="status">Clicked: {clicked}</pre>
    </main>
  </AppContent>
</div>

<style>
  /* These classes are only needed because the
    drawer is in a container on the page. */
  .drawer-container {
    position: relative;
    display: flex;
    height: 350px;
    max-width: 600px;
    border: 1px solid
      var(--mdc-theme-text-hint-on-background, rgba(0, 0, 0, 0.1));
    overflow: hidden;
    z-index: 0;
  }

  * :global(.app-content) {
    flex: auto;
    overflow: auto;
    position: relative;
    flex-grow: 1;
  }

  .main-content {
    overflow: auto;
    padding: 16px;
    height: 100%;
    box-sizing: border-box;
  }
</style>
