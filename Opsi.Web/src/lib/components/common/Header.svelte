<script lang="ts">
  import { Header, SideNav, SideNavMenu, SideNavMenuItem, SideNavItems, SideNavLink } from "../../../../node_modules/carbon-components-svelte";
  import { Settings, UserSettings } from "carbon-icons-svelte";

  let isSideNavOpen: boolean;
</script>

<Header platformName="OPSI.Web" bind:isSideNavOpen>
  <SideNav bind:isOpen={isSideNavOpen} rail={true}>
    <SideNavItems>
      <SideNavLink icon={Settings} text="Configuration" />
      <SideNavMenu icon={UserSettings} text="User mode">
        <SideNavMenuItem href="~" isSelected={true} text="Administrator" />
        <SideNavMenuItem href="~" text="Freelancer" />
      </SideNavMenu>
    </SideNavItems>
  </SideNav>
</Header>
