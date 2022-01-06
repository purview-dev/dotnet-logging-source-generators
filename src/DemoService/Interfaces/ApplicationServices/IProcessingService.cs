using DemoService.Models;

namespace DemoService.Interfaces.ApplicationServices;

interface IProcessingService
{
	void Process(Guid contextId, SomeData someData);
}
