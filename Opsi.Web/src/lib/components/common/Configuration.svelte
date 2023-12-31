<script lang="ts">
  import { fade } from "svelte/transition";
  import { Button, Column, Form, FormGroup, Grid, NumberInput, Row, TextInput } from "carbon-components-svelte";
  import Configuration from "../../Models/Configuration/Configuration";
  import { loadConfiguration } from "@/lib/services/configurationService";
  import type { Readable } from "svelte/store";
  
  let config: Configuration = loadConfiguration();
  const fetchCount = (config.ui.projects.fetchCount as any) as Readable<number>;

  const filePickerOptions: OpenFilePickerOptions = {
    types: [{
      accept: {
          "application/json": [".json"]
      },
      description: "OPSI.Web configuration"
    }],
    excludeAcceptAllOption: true,
    multiple: false,
  };

  async function getFile() {
    
    let file: File;
    let fileHandles: FileSystemFileHandle[];

    try {
        do {
            fileHandles = await window.showOpenFilePicker(filePickerOptions);
            if (fileHandles.length !== 1) {
                alert("Select 1 file.");
            }
        } while (fileHandles.length !== 1)

        file = await fileHandles[0].getFile();
    } catch (err: any) {
        if (err.message === "window.showOpenFilePicker is not a function") {
            alert("The file system API has not been enabled on this browser.\n\nIt may be protected behind an experimental flag.")
        } else {
            console.error(err);
        }
        return;
    }

    try {
        config = JSON.parse(await file.text()) as Configuration;
    } catch (err) {
        console.error(err);
    }
  }
</script>

<div in:fade="{{duration: 500}}">

  <Grid fullWidth noGutter>
    <Row>
      <Column>
        <h1>Configuration</h1>
      </Column>
    </Row>
    <Row>
      <Column>
        <p>The various properties may be configured here.</p>
      </Column>
    </Row>
    <Row>
      <Column sm={4} md={4} lg={6}>
        <h3>Users</h3>
        <p>Configure the usernames.</p>
        <Form>
          <FormGroup>
            <TextInput
              invalidText="An administrator username must be specified."
              labelText="Administrator"
              placeholder="Username of the administrator user"
              required={true}
              bind:value={config.users.administrator.username} />
          </FormGroup>
          <FormGroup>
            <TextInput
              invalidText="A freelancer username must be specified."
              labelText="Freelancer"
              placeholder="Username of the freelancer user"
              required={true}
              bind:value={config.users.freelancer.username} />
          </FormGroup>
        </Form>
      </Column>
    </Row>
    <Row>
      <Column sm={4} md={4} lg={6}>
        <h3>User interface</h3>
        <p>These settings determine the appearance of the application.</p>
        <Form>
          <FormGroup>
            <NumberInput
              helperText="This determines the number of projects which are initially loaded, and then additionally fetched on each load."
              label="Project fetch count"
              max={50}
              min={1}
              bind:value={$fetchCount} />
          </FormGroup>
        </Form>
      </Column>
    </Row>
    <Row>
      <Column sm={4} md={4} lg={6}>
        <h3>Import</h3>
        <p>A configuration may be imported.</p>
        <Button
          size="small"
          on:click={getFile}>
          Select file
        </Button>
      </Column>
    </Row>
  </Grid>
</div>

<style lang="scss">
  $h3_margin_bottom: 32px;
  $h3_margin_top: $h3_margin_bottom * 2;

  h3 {
    margin: $h3_margin_top 0 $h3_margin_bottom;

    & + p {
        margin-bottom: $h3_margin_bottom;
        margin-top: -$h3_margin_bottom;
    }
  }
</style>
