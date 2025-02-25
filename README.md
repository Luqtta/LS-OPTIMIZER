# **LS OPTIMIZER - Aplicativo de Otimização de Windows**

Este projeto visa criar um aplicativo para otimizar o sistema operacional Windows, melhorando o desempenho e a eficiência ao realizar limpezas e ajustes no sistema.

## **Tecnologias Utilizadas**

- **C#**
- **.NET 6/7/8**
- **WPF (Windows Presentation Foundation)**
- **XAML**
- **DispatcherTimer (para atualizações automáticas)**

## **Descrição do Projeto**

O **LS OPTIMIZER** é um aplicativo focado em otimizar o sistema operacional Windows, proporcionando uma maneira fácil de liberar espaço e melhorar o desempenho do computador. Com ele, o usuário pode realizar tarefas como excluir arquivos temporários, cache, prefetch e recent, otimizar a internet e visualizar informações detalhadas sobre o uso de memória do sistema.

### **Principais Funcionalidades**
- **Exclusão de arquivos temporários**: O aplicativo permite excluir a pasta TEMP, PreFetch e Recent.
- **Otimização da internet**: Ajusta configurações do sistema para melhorar a estabilidade e velocidade da conexão.
- **Informações sobre a memória do sistema**: Exibe o uso total da memória, memória livre e tamanho do cache, atualizando periodicamente.
- **Interface simples e eficiente**: Com uma interface gráfica utilizando WPF e XAML, o LS OPTIMIZER oferece uma experiência visual clara e intuitiva.
- **Exclusão automática em intervalos personalizados**: O usuário pode definir a frequência da exclusão para **1, 5, 10, 15, 30 ou 60 minutos**, mantendo o sistema sempre otimizado.
- **Sistema de Status**: O aplicativo possui um sistema de status que informa o andamento da otimização, com animação de pontos para indicar o progresso da operação.
- **Sistema de Logs**: O aplicativo registra todas as ações realizadas, incluindo erros e sucessos, em um arquivo de log para auditoria e análise.

## **Preview**

### **Interface do Aplicativo**

Abaixo está um exemplo da interface do **LS OPTIMIZER**:

![Image](https://github.com/user-attachments/assets/9c2e562d-fae9-4099-ae14-20da8064c3b0)

A interface é simples e direta, oferecendo informações claras sobre o estado da memória do sistema e o status da otimização. O botão de otimização permite ao usuário iniciar o processo de exclusão com um clique.

### **Como o Aplicativo Funciona**

1. Ao iniciar, o aplicativo exibe as informações de memória do sistema em tempo real.
2. O usuário pode clicar no botão de **Otimizar** para liberar espaço no sistema e melhorar a conexão com a internet.
3. O aplicativo pode ser configurado para rodar em segundo plano e ser executado automaticamente na inicialização do Windows.
4. O usuário pode definir a frequência da exclusão automática para manter o sistema sempre limpo e otimizado.
5. O **status** do processo de otimização é atualizado periodicamente, e o usuário é informado sobre o progresso.
6. **Logs detalhados** são gravados em tempo real para que o usuário possa acompanhar as ações realizadas e os erros encontrados.

## **Objetivo**

O objetivo principal do **LS OPTIMIZER** é proporcionar aos usuários uma maneira rápida e simples de otimizar o desempenho do seu computador com Windows, mantendo o sistema limpo e eficiente.

## **Instalação**

1. **Baixar o repositório**: Clone o repositório ou faça o download do código.
2. **Instalar dependências**: Certifique-se de ter o **.NET 6/7/8 SDK** instalado em seu sistema.
3. **Compilar o projeto**: Compile o projeto usando o Visual Studio ou outra IDE que suporte .NET.

## **Como Usar**

1. Abra o aplicativo **LS OPTIMIZER**.
2. O aplicativo exibirá informações sobre a memória do sistema.
3. Clique no botão de **Otimizar** para iniciar o processo de exclusão e otimização da internet.
4. O aplicativo excluirá as pastas de arquivos temporários e aplicará ajustes na conexão com a internet.
5. Acompanhe o **status** da otimização e visualize o progresso com a animação de pontos.
6. Acesse os **logs** para verificar as ações realizadas e erros encontrados.

## **Contribuições**

Contribuições são bem-vindas! Se você deseja melhorar o **LS OPTIMIZER**, sinta-se à vontade para fazer um fork deste repositório e enviar pull requests.
