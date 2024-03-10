import Configuration from "../models/configuration/Configuration";
import { fetchCount } from "../stores/projectsStore";
import { adminAuthToken, adminUsername, freelancerAuthToken, freelancerUsername } from "../stores/usersStore";

const configKey: string = "opsi.config";

export function getConfig(): Configuration {
    let config: Configuration;

    const strConfig = localStorage.getItem(configKey);
    if (strConfig === null) {
        config = getDefault();
    } else {
        try {
            config = JSON.parse(strConfig) as Configuration;
        } catch {
            localStorage.removeItem(configKey);
            console.error("A stored configuration was invalid and has been removed.");
            config = getDefault();
        }
    }

    return config;
}

export function initConfig(): void {
    const config = getConfig();
    setConfig(config);
}

export function setConfig(configuration: Configuration): void {
    localStorage.setItem(configKey, JSON.stringify(configuration));
    fetchCount.update((_) => configuration.ui.projects.fetchCount);
    adminAuthToken.update((_) => configuration.users.administrator.authToken);
    adminUsername.update((_) => configuration.users.administrator.username);
    freelancerAuthToken.update((_) => configuration.users.freelancer.authToken);
    freelancerUsername.update((_) => configuration.users.freelancer.username);
}

function getDefault(): Configuration {
    return new Configuration();
}
