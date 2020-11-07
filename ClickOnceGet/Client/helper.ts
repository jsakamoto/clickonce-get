namespace ClickOnceGet.Client.Helper {
    export function startClickOnce(url: string): void {
        const anchorElement = document.createElement('a');
        anchorElement.href = url;
        anchorElement.click();
        anchorElement.remove();
    }
}