<script lang="ts">
  import { Header,
           HeaderAction,
           HeaderActionLink,
           HeaderSearch,
           HeaderPanelDivider,
           HeaderPanelLink,
           HeaderPanelLinks,
           HeaderUtilities,
           SkipToContent,
           ToastNotification } from "../../../../node_modules/carbon-components-svelte";
  import { Credentials, ErrorOutline, LogoGithub, Settings } from "carbon-icons-svelte";
  import { errors } from "../../stores/errorsStore";

  const SidePanelDisplayDuration = 200;

  let active = false;
  let isErrorsOpen: boolean;
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
    {#if $errors.length > 0}
      <HeaderAction icon={ErrorOutline} transition="{{duration: SidePanelDisplayDuration}}" bind:isErrorsOpen>
        {#each $errors as error (error.uniqueId)}
          <ToastNotification
            caption={new Date().toLocaleString()}
            fullWidth={true}
            subtitle={error.message}
            title={error.displayTitle}
            on:close={(_) => {
              errors.remove(error);
            }} />
        {/each}
      </HeaderAction>
    {/if}
    <HeaderAction icon={Credentials} transition="{{duration: SidePanelDisplayDuration}}" bind:isOpen>
      <HeaderPanelLinks>
        <HeaderPanelDivider>User mode</HeaderPanelDivider>
        <HeaderPanelLink href="#/administrator">Administrator</HeaderPanelLink>
        <HeaderPanelLink href="#/freelancer">Freelancer</HeaderPanelLink>
      </HeaderPanelLinks>
    </HeaderAction>
    <HeaderActionLink icon="{Settings}" href="#/config" />
    <HeaderActionLink icon="{LogoGithub}" href="https://github.com/carbon-design-system/carbon-components-svelte" target="_blank" />
  </HeaderUtilities>
</Header>
