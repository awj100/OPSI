<script lang="ts">
  import { Header, HeaderAction, HeaderActionLink, HeaderSearch, HeaderPanelDivider, HeaderPanelLink, HeaderPanelLinks, HeaderUtilities, SkipToContent } from "../../../../node_modules/carbon-components-svelte";
  import { Credentials, LogoGithub, Settings } from "carbon-icons-svelte";

  let active = false;
  let isOpen: boolean;
  let results: any;
  let value = "";
</script>

<Header platformName="OPSI.Web">
  <svelte:fragment slot="skip-to-content">
    <SkipToContent />
  </svelte:fragment>
  <HeaderUtilities>
    // https://github.com/carbon-design-system/carbon-components-svelte/blob/master/docs/src/pages/_layout.svelte
    <HeaderSearch
      bind:value
      bind:active
      placeholder="Search"
      results="{results}" />
    <HeaderAction icon={Credentials} transition="{{duration: 200}}" bind:isOpen>
      <HeaderPanelLinks>
        <HeaderPanelDivider>User mode</HeaderPanelDivider>
        <HeaderPanelLink href="/#administrator">Administrator</HeaderPanelLink>
        <HeaderPanelLink href="/#freelancer">Freelancer</HeaderPanelLink>
      </HeaderPanelLinks>
    </HeaderAction>
    <HeaderActionLink icon="{Settings}" href="#config" />
    <HeaderActionLink icon="{LogoGithub}" href="https://github.com/carbon-design-system/carbon-components-svelte" target="_blank" />
  </HeaderUtilities>
</Header>
